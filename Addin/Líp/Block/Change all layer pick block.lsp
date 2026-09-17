(defun c:LayerChangeBlock ( / ss i ent obj blkname blkdef doc laylist lay idx layername)

  (setq doc (vla-get-ActiveDocument (vlax-get-Acad-Object)))

  ;; ===== LẤY DANH SÁCH LAYER THEO STT GỐC =====
  (setq laylist '())
  (setq lay (tblnext "LAYER" T))
  (setq i 1)

  (princ "\nDanh sách layer (theo STT gốc):")

  (while lay
    (setq laylist (append laylist (list (cdr (assoc 2 lay)))))
    (princ (strcat "\n " (itoa i) ". " (cdr (assoc 2 lay))))
    (setq i (1+ i))
    (setq lay (tblnext "LAYER"))
  )

  ;; ===== CHỌN LAYER =====
  (setq idx (getint "\nNhập STT layer cần đổi cho LINE / CIRCLE / PLINE trong block: "))

  (if (or (null idx) (< idx 1) (> idx (length laylist)))
    (progn
      (princ "\n✖ STT không hợp lệ.")
      (exit)
    )
  )

  (setq layername (nth (1- idx) laylist))

  ;; ===== CHỌN NHIỀU BLOCK =====
  (setq ss (ssget '((0 . "INSERT"))))

  (if (null ss)
    (progn
      (princ "\n✖ Không chọn block.")
      (exit)
    )
  )

  ;; ===== DUYỆT BLOCK =====
  (setq i 0)
  (while (< i (sslength ss))
    (setq ent (ssname ss i))
    (setq obj (vlax-ename->vla-object ent))
    (setq blkname (vla-get-EffectiveName obj))
    (setq blkdef (vla-item (vla-get-Blocks doc) blkname))

    ;; ===== ĐỔI LAYER ENTITY TRONG BLOCK =====
    (vlax-for item blkdef
      (if (member (vla-get-ObjectName item)
                  '("AcDbLine"
                    "AcDbCircle"
                    "AcDbArc"
                    "AcDbPolyline"
                    "AcDb2dPolyline"))
        (vla-put-Layer item layername)
      )
    )

    (setq i (1+ i))
  )

  (vla-Regen doc acAllViewports)
  (princ (strcat "\n✔ Đã đổi layer LINE / CIRCLE / PLINE trong block sang: " layername))
  (princ)
)
