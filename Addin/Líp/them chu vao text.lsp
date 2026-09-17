;;; =====================================================
;;; THEM CHU VAO TEXT/MTEXT/MLEADER - Nhập từ Command Line
;;; Hỗ trợ copy nhiều dòng tốt nhất có thể
;;; =====================================================

(defun c:themchuvaotext ( / ss i ent obj typ oldTxt allTxt addTxt insPt txtH txtLayer txtStyle mtx ms )

  (vl-load-com)
  (setq ms (vla-get-ModelSpace (vla-get-ActiveDocument (vlax-get-acad-object))))

  (princ "\nChon TEXT / MTEXT / MLEADER: ")
  (setq ss (ssget '((0 . "TEXT,MTEXT,MULTILEADER"))))

  (if ss
    (progn
      (princ "\n=== Nhập nội dung cần THÊM từ Command Line ===")
      (princ "\n- Gõ từng dòng rồi Enter")
      (princ "\n- Copy nhiều dòng từ ngoài (Excel/Word...) rồi Paste vào đây")
      (princ "\n- Nhập dòng trống (chỉ Enter) để KẾT THÚC\n")

      (setq allTxt "")

      ;; Nhập từ command line - phiên bản ổn định
      (while (and (setq addTxt (getstring T "\nDòng text: ")) (/= addTxt ""))
        (if (= allTxt "")
          (setq allTxt addTxt)
          (setq allTxt (strcat allTxt "\\P" addTxt))
        )
      )

      (princ "\nĐã kết thúc nhập nội dung.")

      (if (/= allTxt "")
        (progn
          (setq i 0)
          (repeat (sslength ss)
            (setq ent (ssname ss i))
            (setq obj (vlax-ename->vla-object ent))
            (setq typ (vla-get-ObjectName obj))
            (setq oldTxt (vla-get-TextString obj))

            (if (= typ "AcDbText")
              ;; TEXT -> chuyển thành MTEXT
              (progn
                (setq insPt (cdr (assoc 10 (entget ent))))
                (setq txtH (vla-get-Height obj))
                (setq txtLayer (vla-get-Layer obj))
                (setq txtStyle (vla-get-StyleName obj))

                (setq mtx (vla-AddMText ms (vlax-3d-point insPt) 0 (strcat oldTxt "\\P" allTxt)))

                (vla-put-Height mtx txtH)
                (vla-put-Layer mtx txtLayer)
                (vla-put-StyleName mtx txtStyle)
                (vla-delete obj)
              )
              ;; MTEXT hoặc MULTILEADER
              (vla-put-TextString obj (strcat oldTxt "\\P" allTxt))
            )
            (setq i (1+ i))
          )
          (prompt (strcat "\nĐã thêm nội dung vào " (itoa (sslength ss)) " đối tượng."))
        )
        (prompt "\nKhông có nội dung để thêm.")
      )
    )
    (prompt "\nKhông có đối tượng nào được chọn.")
  )
  (princ)
)