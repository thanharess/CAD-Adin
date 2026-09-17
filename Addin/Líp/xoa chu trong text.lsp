;;; =====================================================
;;; XOACHU - XÓA CỤM CHỮ / TỪ (KHÔNG PHÂN BIỆT HOA THƯỜNG)
;;; =====================================================

(defun c:xoachutrongtext ( / ss i ent obj typ oldTxt allRemove removeTxt newTxt insPt txtH txtLayer txtStyle mtx ms )

  (vl-load-com)
  (setq ms (vla-get-ModelSpace (vla-get-ActiveDocument (vlax-get-acad-object))))

  (princ "\nChon TEXT / MTEXT / MLEADER: ")
  (setq ss (ssget '((0 . "TEXT,MTEXT,MULTILEADER"))))

  (if ss
    (progn
      (princ "\n=== Nhập CHỮ / CỤM TỪ cần XÓA (nhiều dòng) ===")
      (princ "\n- Gõ từng cụm từ cần xóa rồi Enter")
      (princ "\n- Copy nhiều cụm từ từ ngoài rồi Paste")
      (princ "\n- Nhập dòng trống (chỉ Enter) để KẾT THÚC\n")

      (setq allRemove "")

      ;; Nhập các cụm từ cần xóa
      (while (and (setq removeTxt (getstring T "\nCụm từ cần xóa: ")) (/= removeTxt ""))
        (if (= allRemove "")
          (setq allRemove removeTxt)
          (setq allRemove (strcat allRemove "\\P" removeTxt))
        )
      )

      (princ "\nĐã kết thúc nhập nội dung cần xóa.")

      (if (/= allRemove "")
        (progn
          (setq i 0)
          (repeat (sslength ss)
            (setq ent (ssname ss i))
            (setq obj (vlax-ename->vla-object ent))
            (setq typ (vla-get-ObjectName obj))
            (setq oldTxt (vla-get-TextString obj))

            (if (and oldTxt (/= oldTxt ""))
              (progn
                (setq newTxt oldTxt)

                ;; Xóa không phân biệt hoa thường
                (foreach r (LM:str->lst allRemove "\\P")
                  (if (/= r "")
                    (setq newTxt (LM:replace-ignore-case newTxt r ""))
                  )
                )

                ;; Dọn dẹp khoảng trắng thừa
                (while (vl-string-search "  " newTxt)
                  (setq newTxt (vl-string-subst " " "  " newTxt))
                )
                (setq newTxt (vl-string-trim " " newTxt))

                (if (= typ "AcDbText")
                  ;; TEXT -> chuyển thành MTEXT
                  (progn
                    (setq insPt (cdr (assoc 10 (entget ent))))
                    (setq txtH (vla-get-Height obj))
                    (setq txtLayer (vla-get-Layer obj))
                    (setq txtStyle (vla-get-StyleName obj))

                    (if (/= newTxt "")
                      (progn
                        (setq mtx (vla-AddMText ms (vlax-3d-point insPt) 0 newTxt))
                        (vla-put-Height mtx txtH)
                        (vla-put-Layer mtx txtLayer)
                        (vla-put-StyleName mtx txtStyle)
                      )
                    )
                    (vla-delete obj)
                  )
                  ;; MTEXT hoặc MULTILEADER
                  (vla-put-TextString obj newTxt)
                )
              )
            )
            (setq i (1+ i))
          )
          (prompt (strcat "\nĐã xử lý xóa cụm từ trong " (itoa (sslength ss)) " đối tượng."))
        )
        (prompt "\nKhông có nội dung cần xóa.")
      )
    )
    (prompt "\nKhông có đối tượng nào được chọn.")
  )
  (princ)
)

;; =============================================
;; HÀM HỖ TRỢ
;; =============================================
(defun LM:str->lst ( str del / pos )
  (if (setq pos (vl-string-search del str))
    (cons (substr str 1 pos) (LM:str->lst (substr str (+ pos 1 (strlen del))) del))
    (list str)
  )
)

;; Hàm thay thế KHÔNG PHÂN BIỆT HOA THƯỜNG
(defun LM:replace-ignore-case (str old new / pos len)
  (setq len (strlen old))
  (while (setq pos (vl-string-search (strcase old) (strcase str)))
    (setq str (strcat (substr str 1 pos) new (substr str (+ pos len 1))))
  )
  str
)